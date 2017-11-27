import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'xc-processing-scripts',
  templateUrl: './processing-scripts.component.html',
  styleUrls: ['./processing-scripts.component.scss']
})
export class ProcessingScriptsComponent implements OnInit {
  sourceScriptAdditionVisible: boolean;

  @Input() scriptProperty: string[] = [];
  @Input() title: string='';

  constructor() { }

  ngOnInit() {
  }

  addScript(target){
    var elem = (<HTMLSelectElement>target);
    var input = (<HTMLSelectElement>elem.previousElementSibling).value;
    if(!this.scriptProperty){
      this.scriptProperty = [];
    }
    this.scriptProperty.push(input);
    this.sourceScriptAdditionVisible = false;
  }

  showScriptAddition(target,targetProperty){
    eval(targetProperty + "= true");
    var elem = (<HTMLSelectElement>target);
    elem.parentElement.hidden = true;
    this.sourceScriptAdditionVisible = true;
  }

  editRow(target){
    (<HTMLSelectElement>target).getElementsByTagName("input").item(0).hidden = false; 
  }

  saveRow(target){
    if(target.className.includes("script-definition")){
      var elem = (<HTMLSelectElement>target);
      elem.getElementsByTagName("input").item(0).hidden = true; 
      var idx = this.scriptProperty.findIndex(s=>s === elem.innerText);
      this.scriptProperty[idx] = elem.getElementsByTagName("input").item(0).value;
    }
  }
  removeRow(target){
    var idx = this.scriptProperty.findIndex(s=>s === target);
    this.scriptProperty.splice(idx,1);
  }
}
