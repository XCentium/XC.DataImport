import { Directive, ViewContainerRef } from '@angular/core';

@Directive({ 
   selector: '[xcDeleteModal]' 
})
export class DeleteModalDirective {
   constructor(public viewContainerRef: ViewContainerRef) { }
} 