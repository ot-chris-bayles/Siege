﻿class ConditionalRegistrationHandler

    def Handle(component)
        if (component.condition == nil)
            rule = ConditionBasedActivationRule.of(component.base, component.conditiontype).new
        else
            rule = ConditionalActivationRule.of(component.base).new 
            evaluation = LambdaCondition.of(component.conditiontype).new component.condition
            rule.set_evaluation evaluation
        end
        
        x = rule.method(:Then).of(component.type).call()
        x
    end

end