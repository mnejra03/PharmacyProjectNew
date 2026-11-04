// src/app/core/components/base-list.component.ts

import {BaseComponent} from './base-component';

export abstract class BaseListComponent<TItem> extends BaseComponent{
  items: TItem[] = [];

  /**
   * Child components provide the concrete data-loading implementation.
   */
  protected abstract loadData(): void;

  /**
   * Call this helper from the child component ngOnInit.
   */
  protected initList(): void {
    this.loadData();
  }
}
