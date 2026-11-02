import { ToastService } from '../toast.service';

export type ToastServiceSpy = {
  [K in keyof ToastService]: ReturnType<typeof vi.fn>;
};

export function createToastServiceSpy(): ToastServiceSpy {
  return {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
    warning: vi.fn(),
    successKey: vi.fn(),
    errorKey: vi.fn(),
  };
}
