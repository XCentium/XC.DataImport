import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ExistingMappingsComponent } from './existing-mappings.component';

describe('ExistingMappingsComponent', () => {
  let component: ExistingMappingsComponent;
  let fixture: ComponentFixture<ExistingMappingsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ExistingMappingsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExistingMappingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
