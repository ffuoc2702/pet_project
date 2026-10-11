import { useLayoutEffect, useRef, useState, type ReactNode } from 'react';
import { AppTooltip } from './data-display';

export function AppTableCell({
  children,
  text,
  maxLines,
}: {
  children: ReactNode;
  text: string;
  maxLines: number;
}) {
  const ref = useRef<HTMLSpanElement>(null);
  const [clipped, setClipped] = useState(false);
  useLayoutEffect(() => {
    const element = ref.current;
    if (!element) return;
    const measure = () =>
      setClipped(
        element.scrollHeight > element.clientHeight + 1 ||
          element.scrollWidth > element.clientWidth + 1,
      );
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => observer.disconnect();
  }, [children, maxLines]);
  return (
    <AppTooltip
      title={clipped ? text : undefined}
      trigger={['hover', 'focus']}
      classNames={{ root: 'app-table-cell-tooltip' }}
    >
      <span
        ref={ref}
        className="app-table-cell-content"
        style={{ WebkitLineClamp: maxLines }}
        tabIndex={clipped ? 0 : undefined}
      >
        {children}
      </span>
    </AppTooltip>
  );
}
