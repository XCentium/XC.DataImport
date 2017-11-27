import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RunMappingPageComponent } from './run-mapping-page.component';

describe('RunMappingPageComponent', () => {
  let component: RunMappingPageComponent;
  let fixture: ComponentFixture<RunMappingPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RunMappingPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RunMappingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
