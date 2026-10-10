import { Form, type FormProps, type FormItemProps } from 'antd';
import type { ReactNode } from 'react';
export function AppFormItem<Values = unknown>(props: FormItemProps<Values>) {
  return <Form.Item<Values> validateFirst {...props} />;
}
function FormBase<Values = Record<string, unknown>>(
  props: Omit<FormProps<Values>, 'children'> & { children?: ReactNode },
) {
  return (
    <Form<Values>
      layout="vertical"
      scrollToFirstError={{ focus: true }}
      requiredMark="optional"
      {...props}
    />
  );
}
export const AppForm = /* @__PURE__ */ Object.assign(FormBase, {
  Item: AppFormItem,
  List: Form.List,
  ErrorList: Form.ErrorList,
  useForm: Form.useForm,
  useWatch: Form.useWatch,
});
