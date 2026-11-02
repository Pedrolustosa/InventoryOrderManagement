import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import { provideTestTranslate } from '../i18n/testing/provide-test-translate';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;
  let toastr: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    toastr = {
      success: vi.fn(),
      error: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideTestTranslate(),
        ToastService,
        { provide: ToastrService, useValue: toastr },
      ],
    });

    service = TestBed.inject(ToastService);
  });

  it('forwards success and error messages to ngx-toastr', () => {
    service.success('Saved');
    service.error('Failed');

    expect(toastr.success).toHaveBeenCalledWith('Saved', undefined, undefined);
    expect(toastr.error).toHaveBeenCalledWith('Failed', undefined, undefined);
  });

  it('translates keys before showing toasts', () => {
    const translate = TestBed.inject(TranslateService);
    vi.spyOn(translate, 'instant').mockImplementation((key: string | string[]) => `t:${String(key)}`);

    service.successKey('PRODUCTS.FORM.SUCCESS_CREATED', { name: 'Keyboard' });
    service.errorKey('COMMON.ERROR');

    expect(toastr.success).toHaveBeenCalledWith('t:PRODUCTS.FORM.SUCCESS_CREATED', undefined, undefined);
    expect(toastr.error).toHaveBeenCalledWith('t:COMMON.ERROR', undefined, undefined);
  });
});
