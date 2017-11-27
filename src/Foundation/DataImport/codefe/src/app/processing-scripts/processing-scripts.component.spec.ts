import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ProcessingScriptsComponent } from './processing-scripts.component';

describe('ProcessingScriptsComponent', () => {
  let component: ProcessingScriptsComponent;
  let fixture: ComponentFixture<ProcessingScriptsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ProcessingScriptsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ProcessingScriptsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
