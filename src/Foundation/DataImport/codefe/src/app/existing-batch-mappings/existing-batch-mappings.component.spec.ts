import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ExistingBatchMappingsComponent } from './existing-batch-mappings.component';

describe('ExistingBatchMappingsComponent', () => {
  let component: ExistingBatchMappingsComponent;
  let fixture: ComponentFixture<ExistingBatchMappingsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ExistingBatchMappingsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExistingBatchMappingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
