define([], function() {
  var action = function(context, args) {
      var targetControl = context.app[args.targetControlId],
          functionName = args.functionName,
          sourceControl = context.app[args.sourceControlId],
          sourceProperty = args.sourceProperty,
          selectedItem;

      if (targetControl == null) {
          throw "targetControl not found";
      }
      if (!functionName) {
          throw "functionName is not set";
      }

      if (sourceControl && sourceControl.get(sourceProperty)) {
          selectedItem = sourceControl.get(sourceProperty);
      } else {
          console.debug("Unable to get the property to set");
          return;
      }

      console.log(functionName);
      targetControl.trigger(functionName, selectedItem);
  };

  return action;
});