export type RequiredPermissions = string | readonly string[];

export function hasPermissions(
  granted: readonly string[],
  required?: RequiredPermissions,
) {
  return (
    !required ||
    (typeof required === 'string' ? [required] : required).every((code) =>
      granted.includes(code),
    )
  );
}
