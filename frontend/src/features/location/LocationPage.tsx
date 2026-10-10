import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Form,
  InputNumber,
  Space,
  Spin,
  theme,
} from 'antd';
import { useTranslation } from 'react-i18next';
import { env } from '@/shared/config/env';
import { coordinatesSchema } from './coordinates';
import type { Coordinates } from './coordinates';

const LocationMap = lazy(() =>
  import('./LocationMap').then((module) => ({ default: module.LocationMap })),
);
export default function LocationPage() {
  const { t, i18n } = useTranslation();
  const { token } = theme.useToken();
  const [coordinates, setCoordinates] = useState<Coordinates>({
    latitude: 10.776889,
    longitude: 106.700806,
  });
  const [form] = Form.useForm<Coordinates>();
  const [denied, setDenied] = useState(false);
  const [locating, setLocating] = useState(false);
  const requestId = useRef(0);
  useEffect(
    () => () => {
      requestId.current++;
    },
    [],
  );
  function select(value: Coordinates) {
    if (!coordinatesSchema.safeParse(value).success) return;
    setCoordinates(value);
    form.setFieldsValue(value);
  }
  function locate() {
    const id = ++requestId.current;
    if (!navigator.geolocation) {
      setDenied(true);
      return;
    }
    setLocating(true);
    setDenied(false);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        if (id !== requestId.current) return;
        select({
          latitude: Number(position.coords.latitude.toFixed(6)),
          longitude: Number(position.coords.longitude.toFixed(6)),
        });
        setLocating(false);
      },
      () => {
        if (id !== requestId.current) return;
        setDenied(true);
        setLocating(false);
      },
      { timeout: 10_000, maximumAge: 60_000, enableHighAccuracy: false },
    );
  }
  const dark = token.colorBgBase === '#000';
  const number = new Intl.NumberFormat(i18n.language, {
    maximumFractionDigits: 6,
  });
  return (
    <Card title={t('common:location')}>
      <Space orientation="vertical" style={{ width: '100%' }} size="large">
        <p>{t('common:introLocation')}</p>
        <Button onClick={locate} loading={locating}>
          {t('location:locate')}
        </Button>
        {denied && (
          <Alert title={t('location:denied')} type="warning" showIcon />
        )}
        {env.googleMapsApiKey && env.googleMapsMapId ? (
          <Suspense fallback={<Spin />}>
            <LocationMap
              coordinates={coordinates}
              onChange={select}
              dark={dark}
            />
          </Suspense>
        ) : (
          <Alert title={t('location:missingKey')} type="info" showIcon />
        )}
        <Form
          form={form}
          layout="vertical"
          initialValues={coordinates}
          onFinish={select}
        >
          <div className="form-grid">
            <Form.Item
              name="latitude"
              label={t('location:latitude')}
              rules={[
                {
                  required: true,
                  type: 'number',
                  min: -90,
                  max: 90,
                  message: t('location:validLatitude'),
                },
              ]}
            >
              <InputNumber style={{ width: '100%' }} precision={6} />
            </Form.Item>
            <Form.Item
              name="longitude"
              label={t('location:longitude')}
              rules={[
                {
                  required: true,
                  type: 'number',
                  min: -180,
                  max: 180,
                  message: t('location:validLongitude'),
                },
              ]}
            >
              <InputNumber style={{ width: '100%' }} precision={6} />
            </Form.Item>
          </div>
          <Button htmlType="submit" type="primary">
            {t('location:manual')}
          </Button>
        </Form>
        <div role="status">
          {t('location:selected')}: {number.format(coordinates.latitude)},{' '}
          {number.format(coordinates.longitude)}
        </div>
      </Space>
    </Card>
  );
}
