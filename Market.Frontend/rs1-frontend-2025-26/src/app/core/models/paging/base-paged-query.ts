import {PageRequest} from './page-request';

// Matches the C# BasePagedQuery model.
export class BasePagedQuery {
  paging: PageRequest;

  constructor() {
    this.paging = new PageRequest();
  }
}
