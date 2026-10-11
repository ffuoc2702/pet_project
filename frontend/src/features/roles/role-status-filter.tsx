import { useTranslation } from 'react-i18next';
import { AppSelect } from '@/shared/ui/controls';

export function RoleStatusFilter({
  value,
  onChange,
}: {
  value: string | null;
  onChange: (value: string) => void;
}) {
  const { t } = useTranslation();
  return (
    <AppSelect
      aria-label={t('common:status')}
      value={value === 'active' || value === 'inactive' ? value : 'all'}
      onChange={onChange}
      options={[
        { value: 'all', label: t('common:allStatuses') },
        { value: 'active', label: t('common:active') },
        { value: 'inactive', label: t('common:inactive') },
      ]}
    />
  );
}
