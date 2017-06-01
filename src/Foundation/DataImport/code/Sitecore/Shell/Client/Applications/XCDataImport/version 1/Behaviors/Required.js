//Make sure we have the components we need before trying to register the behavior
define(["sitecore"], function (_sc) {    
    //Create the behavior    
    _sc.Factories.createBehavior("Required", {
        events: {
            "change": "validate"
        },
        initialize: function () {
            this.model.set("isValid", true);
            var isRequired = this.model.get("isRequired");
            if (isRequired) {
                this.model.set("isValid", false);
            }
        },
        //Use the afterRender "event" to add our functionality        
        afterRender: function () {            
            //Locate the actual UI element            
            var control = this.$el;            
            var isRequired = this.model.get("isRequired");
            if (isRequired) {
                this.model.set("isValid", false);
            }
        },
        validate: function (){
            var isRequired = this.model.get("isRequired");
            var value = this.model.get("text");
            if (isRequired && !value) {
                this.model.set("isValid", false);
            }
            //alert("error");
            // add validation error
        }
    });
});