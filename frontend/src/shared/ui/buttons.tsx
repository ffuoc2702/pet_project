import { Button, type ButtonProps } from 'antd';
export function AppButton({ loading = false, ...props }: ButtonProps) {
  const busy = typeof loading === 'boolean' ? loading : true;
  return (
    <Button
      htmlType="button"
      aria-busy={busy}
      aria-label={
        typeof props.children === 'string' ? props.children : undefined
      }
      {...props}
      loading={loading}
    />
  );
}
export function AppIconButton(
  props: Omit<ButtonProps, 'children'> & { 'aria-label': string },
) {
  return <AppButton shape="circle" {...props} />;
}
