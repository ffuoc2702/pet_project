import { AppButton as Button } from './buttons';

import type { ButtonProps } from 'antd';
import { useHref, useLinkClickHandler } from 'react-router-dom';
import type { To } from 'react-router-dom';
import type { MouseEvent } from 'react';

export function NavigationLink({
  to,
  onClick,
  ...props
}: Omit<ButtonProps, 'href'> & { to: To }) {
  const href = useHref(to);
  const navigate = useLinkClickHandler<HTMLAnchorElement>(to, {
    target: props.target,
  });
  return (
    <Button
      {...props}
      href={href}
      onClick={(event) => {
        onClick?.(event);
        // Button renders an anchor whenever href is supplied, preserving modified clicks.
        if (!event.defaultPrevented)
          navigate(event as MouseEvent<HTMLAnchorElement>);
      }}
    />
  );
}
