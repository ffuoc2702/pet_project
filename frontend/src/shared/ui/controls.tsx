import {
  Input,
  InputNumber,
  Select,
  Checkbox,
  Switch,
  type SelectProps,
} from 'antd';
import type { ComponentProps } from 'react';
import { useAppLoading } from './use-app-loading';
export { AppButton, AppIconButton } from './buttons';
export { AppForm, AppFormItem } from './forms';
export function AppInput(props: ComponentProps<typeof Input>) {
  return <Input {...props} />;
}
export function AppPasswordInput(props: ComponentProps<typeof Input.Password>) {
  return <Input.Password maxLength={128} {...props} />;
}
export function AppSearchInput(props: ComponentProps<typeof Input.Search>) {
  return <Input.Search allowClear {...props} />;
}
export function AppTextArea(props: ComponentProps<typeof Input.TextArea>) {
  return <Input.TextArea rows={3} {...props} />;
}
export function AppOtpInput({
  onChange,
  onInput,
  ...props
}: ComponentProps<typeof Input.OTP>) {
  return (
    <Input.OTP
      length={6}
      inputMode="numeric"
      autoComplete="one-time-code"
      {...props}
      onInput={(cells) => {
        onInput?.(cells);
        // Ant's onChange only fires for complete codes. Keep the form updated when a digit is removed too.
        onChange?.(cells.join(''));
      }}
    />
  );
}

export function AppInputNumber(props: ComponentProps<typeof InputNumber>) {
  return <InputNumber {...props} />;
}
export function AppSelect<ValueType = unknown>(props: SelectProps<ValueType>) {
  return <Select<ValueType> {...props} />;
}
export function AppCheckbox(props: ComponentProps<typeof Checkbox>) {
  return <Checkbox {...props} />;
}

export function AppSwitch({
  loading = false,
  ...props
}: ComponentProps<typeof Switch>) {
  useAppLoading(loading);
  return (
    <Switch
      {...props}
      aria-busy={loading}
      loading={false}
      disabled={loading ? true : props.disabled}
    />
  );
}
