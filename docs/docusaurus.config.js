/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  title: 'Composed',
  tagline: 'A tiny library for composing reactive applications',
  url: 'https://manuelroemer.github.io/Composed',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  organizationName: 'Manuel Römer',
  projectName: 'Composed',
  themeConfig: {
    prism: {
      additionalLanguages: ['csharp'],
    },
    navbar: {
      title: 'Composed',
      logo: {
        alt: 'Composed Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'doc',
          docId: 'docs/index',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'doc',
          docId: 'packages/index',
          position: 'left',
          label: 'Packages',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Introduction',
              to: '/docs/index',
            },
            {
              label: 'Packages',
              to: '/docs/packages',
            },
          ],
        },
        {
          title: 'Links',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/manuelroemer/Composed',
            },
            {
              label: 'NuGet',
              href: 'https://www.nuget.org/packages/Composed',
            },
            {
              label: 'Stack Overflow',
              href: 'https://stackoverflow.com/questions/tagged/composed',
            },
          ],
        },
        {
          title: 'Other',
          items: [
            {
              label: 'Imprint (Impressum)',
              to: '/imprint',
            },
          ],
        },
      ],
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: 'https://github.com/manuelroemer/Composed/edit/dev/docs/',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
};
