import { sportDetailQuery } from './queries';
import { routePath } from '@/shared/config/routes';
import { AppTextArea } from '@/shared/ui/controls';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { AppButton as Button } from '@/shared/ui/buttons';
import { AppCard as Card } from '@/shared/ui/data-display';
import { AppForm as Form } from '@/shared/ui/forms';
import {
  AppInput as Input,
  AppInputNumber as InputNumber,
} from '@/shared/ui/controls';
import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Space } from 'antd';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/client';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { UnsavedChanges } from '@/shared/ui/unsaved-changes';
import { useResourceLabel } from '@/shared/navigation/use-resource-label';
import { saveSport, sportKeys } from './api';
import type { SportInput } from './types';
import { sportErrorField } from './validation';

function Editor({ initial, id }: { initial: SportInput; id?: string }) {
  const { t } = useTranslation();
  const { message } = useAppFeedback();
  const navigate = useNavigate();
  const client = useQueryClient();
  const [form] = Form.useForm<SportInput>();
  const [dirty, setDirty] = useState(false);
  const [completedId, setCompletedId] = useState<string>();
  const mutation = useMutation({
    mutationFn: (value: SportInput) =>
      saveSport(
        {
          ...value,
          formats: value.formats
            .map<SportInput['formats'][number]>((format) => ({
              ...format,
              action: format.id ? ('MODIFY' as const) : ('NEW' as const),
            }))
            .concat(
              initial.formats
                .filter(
                  (old) =>
                    old.id && !value.formats.some((item) => item.id === old.id),
                )
                .map((old) => ({ ...old, action: 'DELETE' as const })),
            ),
        },
        id,
      ),
    onSuccess: async (sport) => {
      setDirty(false);
      setCompletedId(sport.id);
      await client.invalidateQueries({ queryKey: sportKeys.all });
      void message.success(t('common:saved'));
    },
    onError: (error) => {
      if (error instanceof ApiError && error.problem.errors) {
        form.setFields(
          Object.keys(error.problem.errors).map((key) => ({
            name: sportErrorField(key),
            errors: [t('validation:invalidField')],
          })),
        );
      }
    },
  });
  // Navigate only after the dirty blocker has rendered its saved state.
  if (completedId && !dirty) {
    return <SavedNavigation id={completedId} />;
  }
  const required = {
    required: true,
    whitespace: true,
    message: t('validation:required'),
  };
  const max = (count: number) => ({
    max: count,
    message: t('validation:max', { count }),
  });
  return (
    <Card title={t(id ? 'common:edit' : 'common:create')}>
      <UnsavedChanges dirty={dirty} />
      <ApiErrorAlert
        error={mutation.error}
        retry={() => {
          window.location.reload();
        }}
      />
      <Form
        form={form}
        layout="vertical"
        initialValues={initial}
        disabled={mutation.isPending}
        onValuesChange={() => setDirty(true)}
        onFinish={(value) => mutation.mutate(value)}
      >
        <div className="form-grid">
          <Form.Item
            label={t('common:code')}
            name="code"
            rules={[required, max(32)]}
          >
            <Input maxLength={32} />
          </Form.Item>
          <Form.Item
            label={t('common:name')}
            name="name"
            rules={[required, max(100)]}
          >
            <Input maxLength={100} />
          </Form.Item>
        </div>
        <Form.Item
          label={t('common:description')}
          name="description"
          rules={[max(200)]}
        >
          <AppTextArea rows={3} maxLength={200} showCount />
        </Form.Item>
        <Form.List
          name="formats"
          rules={[
            {
              validator: async (_, value: unknown[]) => {
                if (!value?.length)
                  throw new Error(t('validation:minOneFormat'));
              },
            },
          ]}
        >
          {(fields, { add, remove }, { errors }) => (
            <>
              {fields.map((field) => (
                <Card
                  key={field.key}
                  size="small"
                  title={t('common:formats')}
                  className="format-card"
                >
                  <Form.Item name={[field.name, 'id']} hidden>
                    <Input />
                  </Form.Item>
                  <div className="form-grid">
                    <Form.Item
                      label={t('common:code')}
                      name={[field.name, 'code']}
                      rules={[required, max(32)]}
                    >
                      <Input />
                    </Form.Item>
                    <Form.Item
                      label={t('common:displayName')}
                      name={[field.name, 'displayName']}
                      rules={[required, max(100)]}
                    >
                      <Input />
                    </Form.Item>
                    <Form.Item
                      label={t('common:sideSize')}
                      name={[field.name, 'sideSize']}
                      rules={[
                        {
                          required: true,
                          type: 'number',
                          min: 1,
                          max: 32767,
                          message: t('validation:sideSize'),
                        },
                      ]}
                    >
                      <InputNumber min={1} max={32767} precision={0} />
                    </Form.Item>
                  </div>
                  <Form.Item
                    label={t('common:description')}
                    name={[field.name, 'description']}
                    rules={[max(200)]}
                  >
                    <AppTextArea rows={2} maxLength={200} showCount />
                  </Form.Item>
                  <Button
                    danger
                    onClick={() => {
                      remove(field.name);
                      setDirty(true);
                    }}
                  >
                    {t('common:remove')}
                  </Button>
                </Card>
              ))}
              <Form.ErrorList errors={errors} />
              <Button
                onClick={() => {
                  add({ sideSize: 1 });
                  setDirty(true);
                }}
              >
                {t('common:addFormat')}
              </Button>
            </>
          )}
        </Form.List>
        <Space className="form-actions">
          <Button
            type="primary"
            htmlType="submit"
            loading={mutation.isPending}
            disabled={
              !!mutation.error &&
              mutation.error instanceof ApiError &&
              mutation.error.status === 409
            }
          >
            {t('common:save')}
          </Button>
          <Button
            onClick={() =>
              navigate(
                id
                  ? routePath('sportDetail', { sportId: id })
                  : routePath('sports'),
              )
            }
          >
            {t('common:cancel')}
          </Button>
        </Space>
      </Form>
    </Card>
  );
}
function SavedNavigation({ id }: { id: string }) {
  const navigate = useNavigate();
  // The effect keeps navigation outside rendering.
  useSavedNavigation(navigate, id);
  return null;
}
function useSavedNavigation(
  navigate: ReturnType<typeof useNavigate>,
  id: string,
) {
  useEffect(() => {
    void navigate(routePath('sportDetail', { sportId: id }), { replace: true });
  }, [navigate, id]);
}
export default function SportFormPage() {
  const { sportId } = useParams();
  const query = useQuery({
    ...sportDetailQuery(sportId || ''),
    enabled: !!sportId,
  });
  useResourceLabel('sport', sportId, query.data?.name);
  const { t } = useTranslation();
  if (sportId && query.isPending) return <Card loading />;
  if (query.error)
    return (
      <ApiErrorAlert
        error={query.error}
        retry={() => {
          void query.refetch();
        }}
      />
    );
  const initial: SportInput = query.data
    ? {
        code: query.data.code,
        name: query.data.name,
        description: query.data.description || undefined,
        formats: query.data.formats
          .filter((format) => format.isActive)
          .map((format) => ({
            ...format,
            description: format.description || undefined,
            action: 'MODIFY',
          })),
      }
    : {
        code: '',
        name: '',
        formats: [{ code: '', displayName: '', sideSize: 1, action: 'NEW' }],
      };
  return (
    <section aria-label={t('common:sports')}>
      <Editor key={sportId || 'new'} initial={initial} id={sportId} />
    </section>
  );
}
