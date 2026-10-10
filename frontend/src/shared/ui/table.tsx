import { Table, Pagination, type TableProps } from 'antd';
import type { ComponentProps } from 'react';
import { useTranslation } from 'react-i18next';
import { AppEmpty } from './data-display';
export function AppPagination(props: ComponentProps<typeof Pagination>) {
  const { t } = useTranslation();
  return (
    <Pagination
      showSizeChanger
      pageSizeOptions={[10, 20, 50, 100]}
      showTotal={(count) => t('common:total', { count })}
      {...props}
    />
  );
}
export function AppTable<Row extends object>(props: TableProps<Row>) {
  const { t } = useTranslation();
  const pagination =
    props.pagination === false
      ? false
      : {
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50, 100],
          showTotal: (count: number) => t('common:total', { count }),
          ...props.pagination,
        };
  return (
    <Table<Row>
      size="middle"
      {...props}
      pagination={pagination}
      locale={{ emptyText: <AppEmpty />, ...props.locale }}
    />
  );
}
