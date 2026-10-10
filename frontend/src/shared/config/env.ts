import { z } from 'zod';

const schema = z.object({
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
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  googleMapsApiKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY || '',
  googleMapsMapId: import.meta.env.VITE_GOOGLE_MAPS_MAP_ID || '',
});
