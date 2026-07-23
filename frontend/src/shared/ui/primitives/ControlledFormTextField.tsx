import { useController } from 'react-hook-form'
import type { Control, FieldPath, FieldValues } from 'react-hook-form'
import { FormTextField } from './FormTextField.tsx'
import type { FormTextFieldProps } from './FormTextField.tsx'

interface ControlledFormTextFieldProps<TFieldValues extends FieldValues> extends Omit<
  FormTextFieldProps,
  'defaultValue' | 'error' | 'inputRef' | 'name' | 'onBlur' | 'onChange' | 'value'
> {
  control: Control<TFieldValues>
  name: FieldPath<TFieldValues>
}

export function ControlledFormTextField<TFieldValues extends FieldValues>({
  control,
  helperText,
  name,
  ...props
}: ControlledFormTextFieldProps<TFieldValues>) {
  const { field, fieldState } = useController({ control, name })

  return (
    <FormTextField
      {...props}
      {...field}
      value={field.value ?? ''}
      error={fieldState.invalid}
      helperText={fieldState.error?.message ?? helperText}
    />
  )
}
