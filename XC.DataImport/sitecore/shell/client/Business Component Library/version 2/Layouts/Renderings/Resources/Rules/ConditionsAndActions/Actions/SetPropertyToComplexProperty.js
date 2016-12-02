define([], function() {
  var action = function(context, args) {
      var targetControl = context.app[args.targetControlId],
          propertyName = args.propertyName,
          sourceControl = context.app[args.sourceControlId],
          sourceProperty = args.sourceProperty,
          sourceSubProperty = args.sourceSubProperty,
          selectedItem;

      if (targetControl == null) {
          throw "targetControl not found";
      }
      if (!propertyName) {
          throw "propertyName is not set";
      }

      if (sourceControl.get(sourceProperty)) {
          var sourceData = sourceControl.get(sourceProperty);
          if (sourceSubProperty.indexOf(".") > -1) {
              var parts = sourceSubProperty.split(".");
              var objectTypeString = "sourceData";
              $(parts).each(function(i) {
                  objectTypeString += "[parts["+i+"]]";
              });
              if (eval(objectTypeString) === "False" || eval(objectTypeString) === "True") {
                  selectedItem = eval(objectTypeString) !== "False";
              } else {
                  selectedItem = eval(objectTypeString);
              }
          } else {
              selectedItem = sourceData[sourceSubProperty];
          }
      } else {
          console.debug("Unable to get the property to set");
          return;
      }

      console.log(propertyName);
      targetControl[propertyName] = selectedItem;
  };

  return action;
});