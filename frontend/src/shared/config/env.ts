import { z } from 'zod';

const schema = z.object({
  appName: z.string().trim().min(1),
  defaultMapLatitude: z.coerce.number().min(-90).max(90),
  defaultMapLongitude: z.coerce.number().min(-180).max(180),
  apiBaseUrl: z
    .string()
    .refine(
      (value) => value.startsWith('/') && !value.startsWith('//'),
      'API must use the same origin',
    ),
  googleMapsApiKey: z.string(),
  googleMapsMapId: z.string(),
});

export const env = schema.parse({
  appName: import.meta.env.VITE_APP_NAME || 'MatchingSports',
  defaultMapLatitude: import.meta.env.VITE_MAP_DEFAULT_LATITUDE ?? 10.776889,
  defaultMapLongitude: import.meta.env.VITE_MAP_DEFAULT_LONGITUDE ?? 106.700806,
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  googleMapsApiKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY || '',
  googleMapsMapId: import.meta.env.VITE_GOOGLE_MAPS_MAP_ID || '',
});
