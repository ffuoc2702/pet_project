export function listParams(params: URLSearchParams) {
  return {
    pageNumber: Math.max(1, Number(params.get('page')) || 1),
    pageSize: Math.min(100, Math.max(1, Number(params.get('size')) || 20)),
    searchString: params.get('search') || '',
  };
}

export function activeFilter(params: URLSearchParams) {
  const status = params.get('status');
  return status === 'active' ? true : status === 'inactive' ? false : undefined;
}
