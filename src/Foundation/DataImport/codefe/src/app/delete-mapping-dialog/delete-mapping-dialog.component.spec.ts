import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DeleteMappingDialogComponent } from './delete-mapping-dialog.component';

describe('DeleteMappingDialogComponent', () => {
  let component: DeleteMappingDialogComponent;
  let fixture: ComponentFixture<DeleteMappingDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DeleteMappingDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DeleteMappingDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
