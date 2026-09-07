import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Field } from '../organisation/master-config';
@Component({
  selector: 'app-form-control',
  imports: [ReactiveFormsModule],
  template: ` @if (field().type === 'checkbox') {
      <label class="check-field"
        ><input type="checkbox" [formControl]="control()" /><span>{{ field().label }}</span></label
      >
    } @else {
      <label [for]="field().key"
        >{{ field().label }}
        @if (field().required) {
          <span class="required">*</span>
        }
      </label>
      @switch (field().type) {
        @case ('select') {
          <select
            [id]="field().key"
            [formControl]="control()"
            [attr.aria-invalid]="invalid"
            [attr.aria-required]="field().required || false"
            [attr.aria-describedby]="field().key + '-hint'"
          >
            <option [ngValue]="null">{{ field().required ? 'Select an option' : 'None' }}</option>
            @for (option of options(); track option.value) {
              <option [value]="option.value">{{ option.label }}</option>
            }
          </select>
        }
        @case ('textarea') {
          <textarea
            [id]="field().key"
            [formControl]="control()"
            rows="3"
            [attr.maxlength]="field().maxLength ?? null"
            [attr.aria-invalid]="invalid"
            [attr.aria-required]="field().required || false"
            [attr.aria-describedby]="field().key + '-hint'"
          ></textarea>
        }
        @default {
          <input
            [id]="field().key"
            [type]="field().type || 'text'"
            [formControl]="control()"
            [attr.min]="field().min ?? null"
            [attr.max]="field().max ?? null"
            [attr.step]="field().type === 'time' ? '1' : field().step || '1'"
            [attr.maxlength]="field().maxLength ?? null"
            [attr.aria-invalid]="invalid"
            [attr.aria-required]="field().required || false"
            [attr.aria-describedby]="field().key + '-hint'"
          />
        }
      }
      <small [id]="field().key + '-hint'" [class.field-error]="invalid">{{
        invalid ? message : field().help || ''
      }}</small>
    }`,
})
export class FormControlComponent {
  field = input.required<Field>();
  control = input.required<FormControl>();
  options = input<{ value: string; label: string }[]>([]);
  get invalid() {
    return this.control().invalid && this.control().touched;
  }
  get message() {
    const errors = this.control().errors || {};
    if (errors['required']) return 'This field is required.';
    if (errors['min']) return 'Minimum value: ' + this.field().min;
    if (errors['max']) return 'Maximum value: ' + this.field().max;
    if (errors['maxlength']) return 'Maximum ' + this.field().maxLength + ' characters.';
    if (errors['email']) return 'Enter a valid email address.';
    return 'Enter a valid value.';
  }
}
