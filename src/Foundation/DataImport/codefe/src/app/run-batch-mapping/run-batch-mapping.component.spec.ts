import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RunBatchMappingComponent } from './run-batch-mapping.component';

describe('RunBatchMappingComponent', () => {
  let component: RunBatchMappingComponent;
  let fixture: ComponentFixture<RunBatchMappingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RunBatchMappingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RunBatchMappingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
