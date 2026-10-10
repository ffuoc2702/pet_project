import '@/shared/i18n';
import { act, render, waitFor } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { LocationMap } from '@/features/location/LocationMap';

const sdk = vi.hoisted(() => {
  const callbacks = new Map<string, (event: unknown) => void>();
  const remove = vi.fn();
  return {
    remove,
    callbacks,
    panTo: vi.fn(),
    marker: {
      map: undefined as unknown,
      position: undefined as unknown,
      addListener: (name: string, callback: (event: unknown) => void) => {
        callbacks.set(name, callback);
        return { remove };
      },
    },
  };
});
vi.mock('@/shared/config/env', () => ({
  env: { googleMapsMapId: 'test-map' },
}));
vi.mock('@/shared/integrations/google-maps/loader', () => ({
  loadGoogleMaps: async () => ({
    maps: {
      Map: class {
        panTo = sdk.panTo;
        addListener(name: string, callback: (event: unknown) => void) {
          sdk.callbacks.set(name, callback);
          return { remove: sdk.remove };
        }
      },
    },
    marker: {
      AdvancedMarkerElement: class {
        constructor() {
          return sdk.marker;
        }
        addListener(name: string, callback: (event: unknown) => void) {
          sdk.callbacks.set(name, callback);
          return { remove: sdk.remove };
        }
      },
    },
  }),
}));
it('selects coordinates, updates marker and cleans up SDK listeners', async () => {
  const onChange = vi.fn();
  const view = render(
    <LocationMap
      coordinates={{ latitude: 10, longitude: 106 }}
      onChange={onChange}
      dark={false}
    />,
  );
  await waitFor(() => expect(sdk.callbacks.has('click')).toBe(true));
  act(() =>
    sdk.callbacks.get('click')?.({
      latLng: { lat: () => 21.0278, lng: () => 105.8342 },
    }),
  );
  expect(onChange).toHaveBeenCalledWith({
    latitude: 21.0278,
    longitude: 105.8342,
  });
  view.rerender(
    <LocationMap
      coordinates={{ latitude: 21.0278, longitude: 105.8342 }}
      onChange={onChange}
      dark={false}
    />,
  );
  expect(sdk.panTo).toHaveBeenLastCalledWith({ lat: 21.0278, lng: 105.8342 });
  view.unmount();
  expect(sdk.remove).toHaveBeenCalledTimes(2);
  expect(sdk.marker.map).toBeNull();
});
