import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { listParams } from '@/shared/api/list-params';
import { useDebouncedCallback } from './use-debounced-callback';

export function useTableQueryParams() {
  const { t } = useTranslation();
  const [params, setParams] = useSearchParams();
  const { pageNumber, pageSize, searchString } = listParams(params);
  const [input, setInput] = useState(searchString);
  const { debounce, cancel, flush } = useDebouncedCallback((value: string) => {
    setParams(
      (previous) => {
        const next = new URLSearchParams(previous);
        if (value) next.set('search', value);
        else next.delete('search');
        next.set('page', '1');
        return next;
      },
      { replace: true },
    );
  });
  useEffect(() => {
    cancel();
    setInput(searchString);
  }, [searchString, cancel]);

  return {
    params,
    searchProps: {
      value: input,
      maxLength: 200,
      onChange: (event: React.ChangeEvent<HTMLInputElement>) => {
        setInput(event.target.value);
        debounce(event.target.value);
      },
      onSearch: () => flush(),
    },
    setFilter: (key: string, value: string) => {
      cancel();
      setParams((previous) => {
        const next = new URLSearchParams(previous);
        if (value && value !== 'all') next.set(key, value);
        else next.delete(key);
        if (input) next.set('search', input);
        else next.delete('search');
        next.set('page', '1');
        return next;
      });
    },
    pagination: (total: number) => ({
      current: pageNumber,
      pageSize,
      total,
      showSizeChanger: true,
      pageSizeOptions: [10, 20, 50, 100],
      showTotal: (count: number) => t('common:total', { count }),
      onChange: (page: number, size: number) => {
        cancel();
        setParams((previous) => {
          const next = new URLSearchParams(previous);
          next.set('page', String(size === pageSize ? page : 1));
          next.set('size', String(size));
          return next;
        });
      },
    }),
  };
}
