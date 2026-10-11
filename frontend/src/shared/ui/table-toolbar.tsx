import type { ReactNode } from 'react';
import './table.css';

export function AppTableToolbar({
  search,
  filters,
  options,
}: {
  search?: ReactNode;
  filters?: ReactNode;
  options?: ReactNode;
}) {
  return (
    <div className="app-table-toolbar">
      {search && <div className="app-table-toolbar-search">{search}</div>}
      {filters && <div className="app-table-toolbar-filters">{filters}</div>}
      {options && <div className="app-table-toolbar-options">{options}</div>}
    </div>
  );
}
