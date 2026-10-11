import { Table, Pagination, type TableProps } from 'antd';
import {
  isValidElement,
  useMemo,
  type ComponentProps,
  type ReactNode,
} from 'react';
import type { ColumnType, ColumnGroupType } from 'antd/es/table';
import { useTranslation } from 'react-i18next';
import { useAppLoading } from './use-app-loading';
import { AppEmpty } from './data-display';
import { AppTableCell } from './table-cell';
import './table.css';
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
export type TableBorders = { horizontal: boolean; vertical: boolean };
export type AppTableColumn<Row extends object> = ColumnType<Row> & {
  maxLines?: number | false;
};
export type AppTableColumns<Row extends object> = (
  | AppTableColumn<Row>
  | (Omit<ColumnGroupType<Row>, 'children'> & {
      children: AppTableColumns<Row>;
    })
)[];
export type AppTableProps<Row extends object> = Omit<
  TableProps<Row>,
  'columns'
> & {
  borders?: Partial<TableBorders>;
  columns?: AppTableColumns<Row>;
  maxCellLines?: number | false;
};

function cellText(content: ReactNode): string | undefined {
  if (typeof content === 'string' || typeof content === 'number')
    return String(content);
  if (
    isValidElement<{ children?: ReactNode; to?: unknown; href?: string }>(
      content,
    ) &&
    (content.props.to !== undefined || content.props.href !== undefined)
  )
    return cellText(content.props.children);
}

function clampColumns<Row extends object>(
  columns: AppTableColumns<Row> | undefined,
  defaultLines: number | false,
): TableProps<Row>['columns'] {
  return columns?.map((column) => {
    if ('children' in column)
      return {
        ...column,
        children: clampColumns(column.children, defaultLines)!,
      };
    const { maxLines = defaultLines, ...rest } = column;
    if (maxLines === false) return rest;
    return {
      ...rest,
      render: (value, record, index) => {
        const content = column.render
          ? column.render(value, record, index)
          : value;
        const text = cellText(content);
        return text === undefined ? (
          content
        ) : (
          <AppTableCell
            text={text}
            maxLines={Math.max(1, Math.floor(maxLines))}
          >
            {content}
          </AppTableCell>
        );
      },
    };
  });
}
export function AppTable<Row extends object>({
  borders,
  columns,
  maxCellLines = 3,
  ...props
}: AppTableProps<Row>) {
  const { t } = useTranslation();
  const displayedColumns = useMemo(
    () => clampColumns(columns, maxCellLines),
    [columns, maxCellLines],
  );
  const pagination =
    props.pagination === false
      ? false
      : {
          showSizeChanger: true,
          pageSizeOptions: [10, 20, 50, 100],
          showTotal: (count: number) => t('common:total', { count }),
          ...props.pagination,
        };
  useAppLoading(
    typeof props.loading === 'object'
      ? !!props.loading.spinning
      : !!props.loading,
  );
  return (
    <Table<Row>
      size="middle"
      {...props}
      columns={displayedColumns}
      bordered={borders?.vertical ?? props.bordered}
      className={[
        props.className,
        borders?.horizontal === false && 'app-table-no-horizontal',
        borders?.vertical === false && 'app-table-no-vertical',
      ]
        .filter(Boolean)
        .join(' ')}
      loading={false}
      pagination={pagination}
      locale={{ emptyText: <AppEmpty />, ...props.locale }}
    />
  );
}
