import { LanguageService } from './language.service';

/** Lets Angular pipes read the active locale after runtime language switches. */
export class DynamicLocaleId extends String {
  constructor(private readonly language: LanguageService) {
    super();
  }

  override toString(): string {
    return this.language.localeId();
  }

  override valueOf(): string {
    return this.language.localeId();
  }
}
