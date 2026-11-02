{% set lang = config.theme.language if config.theme.language in api_data else 'en' %}
{% set current_api = api_data.get(lang, api_data['en']) %}

{# --- Dynamically count endpoints --- #}
{% set ns = namespace(endpoint_count=0) %}
{% for controller_name, controller in current_api.controllers.items() %}
    {% for path, data in controller.items() %}
        {% if path.startswith('/') %}
            {% set ns.endpoint_count = ns.endpoint_count + (data | length if data is not mapping else 1) %}
        {% endif %}
    {% endfor %}
{% endfor %}

{# --- Page Header & Dynamic Stats --- #}
{{ current_api.meta.page_title | default('# API Reference') }}

**Version**: {{ current_api.meta.version | default('1.8.2') }}  
**Endpoints total count**: {{ ns.endpoint_count }}

!!! warning "Warning"
    Please read our [API Conventions](developer_guide/api_conventions.md) page to understand our `snake_case` JSON requirements and header rules.

{% if current_api.meta.section and current_api.meta.section.Core_API %}
{{ current_api.meta.section.Core_API.title | default('') }}
{{ current_api.meta.section.Core_API.desc | default('') }}
{% endif %}

{% for controller_name, controller in current_api.controllers.items() %}

{{ controller.title | default('### ' ~ controller_name) }}
{{ controller.desc | default('') }}

{% for path, data in controller.items() %}
{% if path.startswith('/') %}
{# A path that answers several methods holds a list of blocks, one per method;
   a single-method path holds the block itself. #}
{% set blocks = [data] if data is mapping else data -%}
{% for block in blocks -%}
{% set suffix = '' if loop.first else '-' ~ (block.method | default('GET') | lower) -%}
---

<h4 id="{{ path | replace('/', '') | replace('-', '') | replace('{', '') | replace('}', '') | lower }}{{ suffix }}">
<div class="doc-api-container">
<div class="doc-api-header">
<div class="doc-api-method doc-api-method-{{ block.method | default('GET') | lower }}">{{ block.method | default('GET') | upper }}</div>
<div class="doc-api-endpoint">
<code>
```
{{ path }}
```

</code>
<a class="headerlink" href="#{{ path | replace('/', '') | replace('-', '') | replace('{', '') | replace('}', '') | lower }}{{ suffix }}" title="Permanent link">¶</a>
</div>
</div>
</div>
</h4>

{% if block.tags %}
<div class="doc-api-tags" style="margin-bottom: 15px; display: flex; gap: 8px; flex-wrap: wrap;">
  {% for tag in block.tags %}
  <span class="doc-tag">{{ tag }}</span>
  {% endfor %}
</div>
{% endif %}

{{ block.desc | default('') }}

{{ block.curl | default('') }}

{{ block.request | default('') }}

{{ block.response | default('') }}

{% if block.github_link %}

<div class="doc-github-container">
<a href="{{ block.github_link }}" class="doc-github-link">
{{ block.github_name | default('View Source') }}
</a>
</div>
{% endif %}

{% endfor %}
{% endif %}
{% endfor %}

{% endfor %}
