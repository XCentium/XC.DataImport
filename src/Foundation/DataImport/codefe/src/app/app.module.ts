import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { ScAccountInformationModule } from '@speak/ng-bcl/account-information';
import { ScActionBarModule } from '@speak/ng-bcl/action-bar';
import { ScApplicationHeaderModule } from '@speak/ng-bcl/application-header';
import { ScButtonModule } from '@speak/ng-bcl/button';
import { ScGlobalHeaderModule } from '@speak/ng-bcl/global-header';
import { ScGlobalLogoModule } from '@speak/ng-bcl/global-logo';
import { ScIconModule } from '@speak/ng-bcl/icon';
import { ScPageModule } from '@speak/ng-bcl/page';
import { ScMenuModule } from '@speak/ng-bcl/menu';
import { ScDropdownModule } from '@speak/ng-bcl/dropdown';
import { ScTableModule } from '@speak/ng-bcl/table';
import { ScTabsModule } from '@speak/ng-bcl/tabs';
import { CONTEXT, DICTIONARY } from '@speak/ng-bcl';

import { NgScModule } from '@speak/ng-sc';
import { SciAntiCSRFModule } from '@speak/ng-sc/anti-csrf';

import { AppComponent } from './app.component';
import { ExistingMappingsComponent } from './existing-mappings/existing-mappings.component';

import { ItemService } from './item.service';
import { EditMappingPageComponent } from './edit-mapping-page/edit-mapping-page.component';
import { RunMappingPageComponent } from './run-mapping-page/run-mapping-page.component';
import { ScEditableTable } from './editable-table/editable-table.component';
import { TreeModule } from 'angular-tree-component';
import { DataSourcesComponent } from './data-sources/data-sources.component';
import { ProcessingScriptsComponent } from './processing-scripts/processing-scripts.component';
import { TargetSystemComponent } from './target-system/target-system.component';
import { ScDialogModule, ScDialogService, ScDialogBackdrop, ScDialogWindow } from '@speak/ng-bcl/dialog';
import { ScriptEditingDialogComponent } from './script-editing-dialog/script-editing-dialog.component';
import { DeleteMappingDialogComponent } from './delete-mapping-dialog/delete-mapping-dialog.component';
import { MainNavigationComponent } from './main-navigation/main-navigation.component';
import { DeleteModalDirective } from './delete-mapping-dialog/modal.directive';
import { ResultsDirective } from './run-mapping-page/results.directive';
import { ModalDirective } from './script-editing-dialog/modal.directive';
import { ClickOutsideModule } from 'ng4-click-outside';

@NgModule({
  declarations: [
    AppComponent,
    ExistingMappingsComponent,
    EditMappingPageComponent,
    RunMappingPageComponent,
    ScEditableTable,
    DataSourcesComponent,
    ProcessingScriptsComponent,
    TargetSystemComponent,   
    ScriptEditingDialogComponent, 
    DeleteMappingDialogComponent, 
    MainNavigationComponent,
    DeleteModalDirective,
    ResultsDirective,
    ModalDirective
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpModule,
    HttpClientModule,
    RouterModule.forRoot([
      { path: '',   redirectTo: '/existing-mappings', pathMatch: 'full' },
      { path: 'existing-mappings', component: ExistingMappingsComponent },
      { path: 'edit-mapping/:mappingId', component: EditMappingPageComponent  },
      { path: 'create-mapping/:mappingId', component: EditMappingPageComponent  },
      { path: 'field-scripts', component: ScriptEditingDialogComponent },
      { path: 'run-mapping/:mappingId', component: RunMappingPageComponent },
      { path: 'delete-mapping', component: DeleteMappingDialogComponent }
    ]),
    ScAccountInformationModule,
    ScActionBarModule,
    ScApplicationHeaderModule,
    ScButtonModule,
    ScGlobalHeaderModule,
    ScGlobalLogoModule,
    ScIconModule,
    ScPageModule,
    SciAntiCSRFModule,
    ScMenuModule,
    ScDropdownModule,
    ScTableModule,
    ScTabsModule,
    TreeModule,
    ScDialogModule,
    ClickOutsideModule,
    NgScModule.forRoot({
      // The ItemId refers to '/sitecore/client/Applications/XcMigrationTool/UserAccess' AccessFolder item
      authItemId: 'CA0E53EF-D1D2-43F4-9750-3BB24A9D2936',
      contextToken: CONTEXT,
      dictionaryToken: DICTIONARY,
      // The ItemId refers to '/sitecore/client/Applications/XcMigrationTool/Translations' Speak3DictionaryFolder item
      translateItemId: '8E159D2D-E688-4638-964D-E3B20DDC2247'
    })
  ],
  providers: [
    ItemService,
    ScDialogService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
