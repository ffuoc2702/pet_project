import { createContext } from 'react';

export const LoadingContext = createContext<{
  register: () => () => void;
} | null>(null);
