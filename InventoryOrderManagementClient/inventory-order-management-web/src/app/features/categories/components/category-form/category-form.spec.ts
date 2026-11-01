import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateCategoryRequest } from '../../models/create-category-request.model';
import { CategoryForm } from './category-form';

describe('CategoryForm', () => {
  let fixture: ComponentFixture<CategoryForm>;
  let component: CategoryForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryForm],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should emit a trimmed payload for valid data', () => {
    const emissions: CreateCategoryRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: ' Electronics ',
      description: ' Devices and gadgets ',
    });

    component.submit();

    expect(emissions).toEqual([
      {
        name: 'Electronics',
        description: 'Devices and gadgets',
      },
    ]);
  });

  it('should emit null description when blank after trim', () => {
    const emissions: CreateCategoryRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: 'Books',
      description: '   ',
    });

    component.submit();

    expect(emissions).toEqual([
      {
        name: 'Books',
        description: null,
      },
    ]);
  });

  it('should reject an empty name', () => {
    component.form.controls.name.setValue('');
    expect(component.form.controls.name.hasError('required')).toBe(true);
  });

  it('should reject name longer than 100 characters', () => {
    component.form.controls.name.setValue('N'.repeat(101));
    expect(component.form.controls.name.hasError('maxlength')).toBe(true);
  });

  it('should reject description longer than 300 characters', () => {
    component.form.controls.description.setValue('D'.repeat(301));
    expect(component.form.controls.description.hasError('maxlength')).toBe(true);
  });

  it('should not emit when submitting while invalid', () => {
    const emissions: CreateCategoryRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      name: '',
      description: 'D'.repeat(301),
    });

    component.submit();

    expect(emissions).toEqual([]);
  });

  it('should populate fields when editing an existing category', () => {
    fixture.componentRef.setInput('mode', 'edit');
    fixture.componentRef.setInput('category', {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Electronics',
      description: 'Devices',
      isActive: true,
      createdAtUtc: '2026-01-01T00:00:00Z',
      updatedAtUtc: null,
    });
    fixture.detectChanges();

    expect(component.form.getRawValue()).toEqual({
      name: 'Electronics',
      description: 'Devices',
    });
  });
});
