// src/app/modules/shared/services/dialog-helper.service.ts

import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { DialogConfig, DialogType, DialogButton, DialogResult } from '../models/dialog-config.model';
import {FitConfirmDialogComponent} from '../components/fit-confirm-dialog/fit-confirm-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class DialogHelperService {
  constructor(private dialog: MatDialog) {}

  /**
   * Opens a custom dialog with full configuration
   */
  open(config: DialogConfig): Observable<DialogResult | undefined> {
    const dialogRef = this.dialog.open(FitConfirmDialogComponent, {
      width: config.width || '450px',
      disableClose: config.disableClose || false,
      data: config,
      panelClass: 'custom-dialog-container'
    });

    return dialogRef.afterClosed();
  }

  /**
   * Shows a simple info dialog with OK button
   */
  showInfo(titleKey: string, messageKey: string, params?: any, icon?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.INFO,
      titleKey,
      messageKey,
      titleParams: params,
      messageParams: params,
      icon,
      buttons: [
        { type: DialogButton.OK, color: 'primary' }
      ]
    });
  }

  /**
   * Shows a success dialog with OK button
   */
  showSuccess(titleKey: string, messageKey: string, params?: any, icon?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.SUCCESS,
      titleKey,
      messageKey,
      titleParams: params,
      messageParams: params,
      icon,
      buttons: [
        { type: DialogButton.OK, color: 'primary' }
      ]
    });
  }

  /**
   * Shows an error dialog with OK button
   */
  showError(titleKey: string, messageKey: string, params?: any, icon?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.ERROR,
      titleKey,
      messageKey,
      titleParams: params,
      messageParams: params,
      icon,
      buttons: [
        { type: DialogButton.OK, color: 'warn' }
      ]
    });
  }

  /**
   * Shows a warning dialog with OK button
   */
  showWarning(titleKey: string, messageKey: string, params?: any, icon?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.WARNING,
      titleKey,
      messageKey,
      titleParams: params,
      messageParams: params,
      icon,
      buttons: [
        { type: DialogButton.OK, color: 'primary' }
      ]
    });
  }

  /**
   * Shows a confirmation dialog with Yes/No buttons
   */
  confirm(titleKey: string, messageKey: string, params?: any, icon?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.QUESTION,
      titleKey,
      messageKey,
      titleParams: params,
      messageParams: params,
      icon,
      buttons: [
        { type: DialogButton.NO },
        { type: DialogButton.YES, color: 'primary' }
      ]
    });
  }

  /**
   * Shows a confirmation dialog with OK/Cancel buttons
   */
  confirmOkCancel(titleKey: string, messageKey: string, params?: any, icon?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.QUESTION,
      titleKey,
      messageKey,
      titleParams: params,
      messageParams: params,
      icon,
      buttons: [
        { type: DialogButton.CANCEL },
        { type: DialogButton.OK, color: 'primary' }
      ]
    });
  }

  /**
   * Shows a delete confirmation dialog
   */
  confirmDelete(itemName: string, messageKey?: string): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.WARNING,
      titleKey: 'DIALOGS.TITLES.CONFIRM_DELETE',
      messageKey: messageKey || 'DIALOGS.MESSAGES.DELETE_CONFIRM',
      messageParams: { name: itemName },
      icon: 'delete_forever',
      buttons: [
        { type: DialogButton.CANCEL },
        { type: DialogButton.DELETE, color: 'warn' }
      ]
    });
  }

  /**
   * Shows unsaved changes confirmation
   */
  confirmUnsavedChanges(): Observable<DialogResult | undefined> {
    return this.open({
      type: DialogType.WARNING,
      titleKey: 'DIALOGS.TITLES.UNSAVED_CHANGES',
      messageKey: 'DIALOGS.MESSAGES.UNSAVED_CHANGES',
      icon: 'warning',
      buttons: [
        { type: DialogButton.NO },
        { type: DialogButton.YES, color: 'primary' }
      ]
    });
  }

  /**
   * Shows a custom dialog with custom buttons
   */
  showCustom(config: DialogConfig): Observable<DialogResult | undefined> {
    return this.open(config);
  }

}
