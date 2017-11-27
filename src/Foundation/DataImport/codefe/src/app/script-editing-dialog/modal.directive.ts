import { Directive, ViewContainerRef } from '@angular/core';

@Directive({ 
   selector: '[xcModal]' 
})
export class ModalDirective {
   constructor(public viewContainerRef: ViewContainerRef) { }
} 