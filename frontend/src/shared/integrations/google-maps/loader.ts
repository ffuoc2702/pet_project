import { importLibrary, setOptions } from '@googlemaps/js-api-loader';
import { env } from '@/shared/config/env';
let configured = false;
let loading:
  | Promise<{
      maps: google.maps.MapsLibrary;
      marker: google.maps.MarkerLibrary;
    }>
  | undefined;
export function loadGoogleMaps() {
  if (!env.googleMapsApiKey || !env.googleMapsMapId)
    return Promise.reject(new Error('MAP_NOT_CONFIGURED'));
  if (!configured) {
    setOptions({ key: env.googleMapsApiKey, v: 'quarterly', region: 'VN' });
    configured = true;
  }
  loading ??= Promise.all([importLibrary('maps'), importLibrary('marker')])
    .then(([maps, marker]) => ({
      maps: maps as google.maps.MapsLibrary,
      marker: marker as google.maps.MarkerLibrary,
    }))
    .catch((error: unknown) => {
      loading = undefined;
      throw error;
    });
  return loading;
}
