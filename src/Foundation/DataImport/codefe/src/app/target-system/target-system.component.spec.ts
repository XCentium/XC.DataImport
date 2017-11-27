import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TargetSystemComponent } from './target-system.component';

describe('TargetSystemComponent', () => {
  let component: TargetSystemComponent;
  let fixture: ComponentFixture<TargetSystemComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TargetSystemComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TargetSystemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
