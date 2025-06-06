{{ if ne .Values.operator.enabled false }}
apiVersion: admissionregistration.k8s.io/v1
kind: MutatingWebhookConfiguration
metadata:
  name: contrast-web-hook-configuration
  labels:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
webhooks:
  - name: pods.agents.contrastsecurity.com
    reinvocationPolicy: IfNeeded
    failurePolicy: Ignore
    timeoutSeconds: 2
    namespaceSelector:
      matchExpressions:
      - key: kubernetes.io/metadata.name
        operator: NotIn
        values:
        - kube-system
        - kube-node-lease
    matchPolicy: Equivalent
    rules:
      - apiGroups: [ "" ]
        apiVersions: [ "v1" ]
        operations: [ "CREATE" ]
        resources: [ "pods" ]
        scope: Namespaced
    clientConfig:
      service:
        name: contrast-agent-operator
        namespace: '{{ default .Release.Namespace .Values.namespace }}'
        path: /mutate/v1pod
    admissionReviewVersions: [ "v1" ]
    sideEffects: None
{{ end }}
