import { Button, type ButtonProps } from 'antd';
export function AppButton(props: ButtonProps) {
  return <Button htmlType="button" {...props} />;
}
export function AppIconButton(
  props: Omit<ButtonProps, 'children'> & { 'aria-label': string },
) {
  return <AppButton shape="circle" {...props} />;
}
