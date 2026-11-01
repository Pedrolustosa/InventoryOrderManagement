import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateSupplierRequest } from '../../models/create-supplier-request.model';
import { SupplierForm } from './supplier-form';

describe('SupplierForm', () => {
  let fixture: ComponentFixture<SupplierForm>;
  let component: SupplierForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupplierForm],
    }).compileComponents();

    fixture = TestBed.createComponent(SupplierForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should emit a trimmed payload for valid data', () => {
    const emissions: CreateSupplierRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: ' Tech Components ',
      taxIdentifier: ' 012.345.678/0001-90 ',
      contactName: ' John ',
      // Email validator runs on raw value; spaces around address make it invalid.
      email: 'contact@tech.com',
      phone: ' 123 ',
      notes: ' Notes ',
    });

    component.submit();

    expect(emissions).toEqual([
      {
        name: 'Tech Components',
        taxIdentifier: '012.345.678/0001-90',
        contactName: 'John',
        email: 'contact@tech.com',
        phone: '123',
        notes: 'Notes',
      },
    ]);
  });

  it('should emit null for blank optional fields', () => {
    const emissions: CreateSupplierRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: 'Local Shop',
      taxIdentifier: '   ',
      contactName: '',
      email: '',
      phone: '',
      notes: '',
    });

    component.submit();

    expect(emissions).toEqual([
      {
        name: 'Local Shop',
        taxIdentifier: null,
        contactName: null,
        email: null,
        phone: null,
        notes: null,
      },
    ]);
  });

  it('should reject an empty name', () => {
    component.form.controls.name.setValue('');
    expect(component.form.controls.name.hasError('required')).toBe(true);
  });

  it('should reject name longer than 150 characters', () => {
    component.form.controls.name.setValue('N'.repeat(151));
    expect(component.form.controls.name.hasError('maxlength')).toBe(true);
  });

  it('should reject contact name longer than 100 characters', () => {
    component.form.controls.contactName.setValue('C'.repeat(101));
    expect(component.form.controls.contactName.hasError('maxlength')).toBe(true);
  });

  it('should reject invalid email format', () => {
    component.form.controls.email.setValue('not-an-email');
    expect(component.form.controls.email.hasError('email')).toBe(true);
  });

  it('should reject email longer than 254 characters', () => {
    component.form.controls.email.setValue(`${'a'.repeat(250)}@x.com`);
    expect(component.form.controls.email.hasError('maxlength')).toBe(true);
  });

  it('should reject phone longer than 30 characters', () => {
    component.form.controls.phone.setValue('1'.repeat(31));
    expect(component.form.controls.phone.hasError('maxlength')).toBe(true);
  });

  it('should reject notes longer than 500 characters', () => {
    component.form.controls.notes.setValue('N'.repeat(501));
    expect(component.form.controls.notes.hasError('maxlength')).toBe(true);
  });

  it('should preserve leading zeros in tax identifier', () => {
    const emissions: CreateSupplierRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: 'Person',
      taxIdentifier: '01234567890',
      contactName: '',
      email: '',
      phone: '',
      notes: '',
    });

    component.submit();

    expect(emissions[0].taxIdentifier).toBe('01234567890');
  });

  it('should not emit when submitting while invalid', () => {
    const emissions: CreateSupplierRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: '',
      taxIdentifier: '',
      contactName: 'C'.repeat(101),
      email: 'bad',
      phone: '1'.repeat(31),
      notes: 'N'.repeat(501),
    });

    component.submit();

    expect(emissions).toEqual([]);
  });

  it('should populate fields when editing an existing supplier', () => {
    fixture.componentRef.setInput('mode', 'edit');
    fixture.componentRef.setInput('supplier', {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Tech Components',
      taxIdentifier: '12.345.678/0001-90',
      contactName: 'John',
      email: 'a@b.com',
      phone: '123',
      notes: 'Notes',
      isActive: true,
      createdAtUtc: '2026-01-01T00:00:00Z',
      updatedAtUtc: null,
    });
    fixture.detectChanges();

    expect(component.form.getRawValue()).toEqual({
      name: 'Tech Components',
      taxIdentifier: '12.345.678/0001-90',
      contactName: 'John',
      email: 'a@b.com',
      phone: '123',
      notes: 'Notes',
    });
  });
});
