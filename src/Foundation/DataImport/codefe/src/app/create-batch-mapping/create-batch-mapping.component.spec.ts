import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateBatchMappingComponent } from './create-batch-mapping.component';

describe('CreateBatchMappingComponent', () => {
  let component: CreateBatchMappingComponent;
  let fixture: ComponentFixture<CreateBatchMappingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CreateBatchMappingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CreateBatchMappingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
