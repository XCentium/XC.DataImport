import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScriptEditingDialogComponent } from './script-editing-dialog.component';

describe('ScriptEditingDialogComponent', () => {
  let component: ScriptEditingDialogComponent;
  let fixture: ComponentFixture<ScriptEditingDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ScriptEditingDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScriptEditingDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
