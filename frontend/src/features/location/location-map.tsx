import { AppAlert as Alert } from '@/shared/ui/data-display';
import { AppButton as Button } from '@/shared/ui/buttons';
import { useEffect, useRef, useState } from 'react';
import { Space, Spin } from 'antd';
import { useTranslation } from 'react-i18next';
import { loadGoogleMaps } from '@/shared/integrations/google-maps/loader';
import { env } from '@/shared/config/env';
import type { Coordinates } from './coordinates';

export function LocationMap({
  coordinates,
  onChange,
  dark,
}: {
  coordinates: Coordinates;
  onChange: (value: Coordinates) => void;
  dark: boolean;
}) {
  const { t } = useTranslation();
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<google.maps.Map | undefined>(undefined);
  const marker = useRef<google.maps.marker.AdvancedMarkerElement | undefined>(
    undefined,
  );
  const current = useRef({ coordinates, onChange });
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>(
    'loading',
  );
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    current.current = { coordinates, onChange };
  }, [coordinates, onChange]);
  useEffect(() => {
    let disposed = false;
    let listeners: google.maps.MapsEventListener[] = [];
    setStatus('loading');
    void loadGoogleMaps()
      .then(({ maps, marker: markers }) => {
        if (disposed || !container.current) return;
        const center = {
          lat: current.current.coordinates.latitude,
          lng: current.current.coordinates.longitude,
        };
        map.current = new maps.Map(container.current, {
          center,
          zoom: 13,
          mapId: env.googleMapsMapId,
          disableDefaultUI: true,
          colorScheme: dark ? 'DARK' : 'LIGHT',
        });
        marker.current = new markers.AdvancedMarkerElement({
          map: map.current,
          position: center,
          gmpDraggable: true,
        });
        const select = (point: google.maps.LatLng) =>
          current.current.onChange({
            latitude: Number(point.lat().toFixed(6)),
            longitude: Number(point.lng().toFixed(6)),
          });
        listeners = [
          map.current.addListener(
            'click',
            (event: google.maps.MapMouseEvent) => {
              if (event.latLng) select(event.latLng);
            },
          ),
          marker.current.addListener(
            'dragend',
            (event: google.maps.MapMouseEvent) => {
              if (event.latLng) select(event.latLng);
            },
          ),
        ];
        setStatus('ready');
      })
      .catch(() => {
        if (!disposed) setStatus('error');
      });
    return () => {
      disposed = true;
      listeners.forEach((listener) => listener.remove());
      if (marker.current) marker.current.map = null;
      marker.current = undefined;
      map.current = undefined;
    };
  }, [dark, attempt]);
  useEffect(() => {
    const position = { lat: coordinates.latitude, lng: coordinates.longitude };
    if (marker.current) marker.current.position = position;
    map.current?.panTo(position);
  }, [coordinates]);
  return (
    <Space orientation="vertical" style={{ width: '100%' }}>
      {status === 'error' && (
        <Alert
          type="warning"
          showIcon
          title={t('location:mapError')}
          action={
            <Button onClick={() => setAttempt((value) => value + 1)}>
              {t('common:tryAgain')}
            </Button>
          }
        />
      )}
      <Spin spinning={status === 'loading'}>
        <div
          ref={container}
          className="map-container"
          aria-label={t('location:mapLabel')}
        />
      </Spin>
      {status === 'ready' && (
        <Space>
          <Button
            aria-label={t('location:zoomIn')}
            onClick={() =>
              map.current?.setZoom((map.current.getZoom() || 13) + 1)
            }
          >
            +
          </Button>
          <Button
            aria-label={t('location:zoomOut')}
            onClick={() =>
              map.current?.setZoom((map.current.getZoom() || 13) - 1)
            }
          >
            −
          </Button>
        </Space>
      )}
    </Space>
  );
}
