import type { SportInput } from './types';
type FormField =
  | keyof SportInput
  | ['formats', number, 'code' | 'displayName' | 'sideSize' | 'description'];
export function sportErrorField(key: string): FormField {
  const root = (['code', 'name', 'description', 'formats'] as const).find(
    (field) => field.toLowerCase() === key.toLowerCase(),
  );
  if (root) return root;
  const match =
    /^formats\[(\d+)\]\.(code|displayName|sideSize|description)$/i.exec(key);
  if (match) {
    const field = (
      ['code', 'displayName', 'sideSize', 'description'] as const
    ).find((item) => item.toLowerCase() === match[2].toLowerCase())!;
    return ['formats', Number(match[1]), field];
  }
  return 'formats';
}
