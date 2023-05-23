import { FunctionalComponent, h } from '@stencil/core';
import { isNullOrWhitespace } from '../../../utils';

export interface InfoListProps {
  title: string;
  dictionary?: { [key: string]: string };
  hideEmptyValues?: boolean;
}

export const InfoList: FunctionalComponent<InfoListProps> = ({ title, dictionary, hideEmptyValues }) => {
  let entries = Object.entries(dictionary);

  if (hideEmptyValues) entries = entries.filter(([k, v]) => !isNullOrWhitespace(v));

  return (
    <div class="tw-p-4">
      <div class="tw-mx-auto">
        <div>
          <div>
            <h3 class="tw-text-base tw-leading-6 tw-font-medium tw-text-gray-900">{title}</h3>
          </div>
          <div class="tw-mt-3 tw-border-t tw-border-gray-200">
            <dl class="sm:tw-divide-y sm:tw-divide-gray-200">
              {entries.map(([k, v]) => (
                <div class="tw-py-3 sm:tw-grid sm:tw-grid-cols-3 sm:gap-4">
                  <dt class="tw-text-sm tw-font-medium tw-text-gray-500">{k}</dt>
                  <dd class="tw-flex tw-justify-between tw-items-center tw-mt-1 tw-text-sm tw-text-gray-900 sm:tw-mt-0 sm:tw-col-span-2">
                    {v}
                    {isNullOrWhitespace(v) ? undefined : <elsa-copy-button value={v} />}
                  </dd>
                </div>
              ))}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
};
