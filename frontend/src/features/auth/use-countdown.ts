import { useEffect, useState } from 'react';
export function useCountdown(deadline: number) {
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    if (!deadline) return;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [deadline]);
  return Math.max(0, Math.ceil((deadline - now) / 1000));
}
