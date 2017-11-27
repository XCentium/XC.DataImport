import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScEditableTable } from './editable-table.component';

describe('ScEditableTable', () => {
  let component: ScEditableTable;
  let fixture: ComponentFixture<ScEditableTable>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ScEditableTable ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScEditableTable);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
