import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';

// Specs render dates with the pt-BR locale. Register it once for every test file instead of relying on
// another spec having registered it first, which made results depend on execution order.
registerLocaleData(localePt);
registerLocaleData(localePt, 'pt-BR');
