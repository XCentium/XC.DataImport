import { Directive, ViewContainerRef } from '@angular/core';

@Directive({ 
   selector: '[xcImportResults]' 
})
export class ResultsDirective {
   constructor(public viewContainerRef: ViewContainerRef) { }
} 