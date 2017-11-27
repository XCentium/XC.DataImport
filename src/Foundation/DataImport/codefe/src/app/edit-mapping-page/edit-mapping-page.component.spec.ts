import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { EditMappingPageComponent } from './edit-mapping-page.component';

describe('EditMappingPageComponent', () => {
  let component: EditMappingPageComponent;
  let fixture: ComponentFixture<EditMappingPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ EditMappingPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EditMappingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
