import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateCustomerRequest } from '../../models/create-customer-request.model';
import { CustomerForm } from './customer-form';

describe('CustomerForm', () => {
  let fixture: ComponentFixture<CustomerForm>;
  let component: CustomerForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerForm],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should emit a trimmed Individual payload without address', () => {
    const emissions: CreateCustomerRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.patchValue({
      customerType: 'Individual',
      name: ' John Smith ',
      taxIdentifier: ' 012.345.678-90 ',
      email: 'john@example.com',
      phone: ' 123 ',
    });

    component.submit();

    expect(emissions).toEqual([
      {
        customerType: 'Individual',
        name: 'John Smith',
        taxIdentifier: '012.345.678-90',
        email: 'john@example.com',
        phone: '123',
        address: null,
      },
    ]);
  });

  it('should emit Company payload with complete address', () => {
    const emissions: CreateCustomerRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.toggleAddress(true);
    component.form.patchValue({
      customerType: 'Company',
      name: 'Acme Ltd.',
      taxIdentifier: '12.345.678/0001-90',
      email: '',
      phone: '',
      postalCode: '50000-000',
      street: 'Example Street',
      number: '100',
      complement: '',
      neighborhood: 'Boa Viagem',
      city: 'Recife',
      state: 'PE',
      country: 'Brazil',
    });

    component.submit();

    expect(emissions[0].customerType).toBe('Company');
    expect(emissions[0].address).toEqual({
      postalCode: '50000-000',
      street: 'Example Street',
      number: '100',
      complement: null,
      neighborhood: 'Boa Viagem',
      city: 'Recife',
      state: 'PE',
      country: 'Brazil',
    });
  });

  it('should reject empty name', () => {
    component.form.controls.name.setValue('');
    expect(component.form.controls.name.hasError('required')).toBe(true);
  });

  it('should reject name longer than 150 characters', () => {
    component.form.controls.name.setValue('N'.repeat(151));
    expect(component.form.controls.name.hasError('maxlength')).toBe(true);
  });

  it('should reject invalid email', () => {
    component.form.controls.email.setValue('bad');
    expect(component.form.controls.email.hasError('email')).toBe(true);
  });

  it('should reject incomplete address when address section is enabled', () => {
    component.toggleAddress(true);
    component.form.patchValue({
      customerType: 'Individual',
      name: 'John',
      postalCode: '',
      street: '',
      number: '',
      city: '',
      state: '',
      country: '',
    });

    component.submit();
    expect(component.form.invalid).toBe(true);
  });

  it('should show CPF label for Individual and CNPJ for Company', () => {
    component.form.controls.customerType.setValue('Individual');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('CPF');

    component.form.controls.customerType.setValue('Company');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('CNPJ');
  });

  it('should preserve name when changing customer type', () => {
    component.form.controls.name.setValue('Keep Me');
    component.form.controls.customerType.setValue('Company');
    expect(component.form.controls.name.value).toBe('Keep Me');
  });

  it('should preserve leading zeros in tax identifier', () => {
    const emissions: CreateCustomerRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.patchValue({
      customerType: 'Individual',
      name: 'Person',
      taxIdentifier: '01234567890',
    });
    component.submit();

    expect(emissions[0].taxIdentifier).toBe('01234567890');
  });
});
