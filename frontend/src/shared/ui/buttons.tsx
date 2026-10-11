import { Button, type ButtonProps } from 'antd';
import { useAppLoading } from './use-app-loading';
export function AppButton({ loading = false, ...props }: ButtonProps) {
  const busy = typeof loading === 'boolean' ? loading : true;
  useAppLoading(busy);
  return (
    <Button
      htmlType="button"
      aria-busy={busy}
      aria-label={
        typeof props.children === 'string' ? props.children : undefined
      }
      {...props}
      disabled={busy ? true : props.disabled}
      loading={false}
    />
  );
}
export function AppIconButton(
  props: Omit<ButtonProps, 'children'> & { 'aria-label': string },
) {
  return <AppButton shape="circle" {...props} />;
}
