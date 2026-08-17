import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Our IQ',
  description: 'Design and documentation for a governed shared knowledge server.',
  base: '/our-iq/',
  outDir: '.vitepress/dist',
  appearance: 'auto',
  cleanUrls: true,
  ignoreDeadLinks: false,
  themeConfig: {
    nav: [
      { text: 'Getting started', link: '/tutorials/' },
      { text: 'How-to guides', link: '/how-to/' },
      { text: 'Reference', link: '/reference/' },
      { text: 'Design', link: '/design/' }
    ],
    sidebar: [
      {
        text: 'Learn',
        items: [
          { text: 'Tutorials', link: '/tutorials/' },
          { text: 'How-to guides', link: '/how-to/' },
          { text: 'Reference', link: '/reference/' },
          { text: 'Explanation', link: '/explanation/' }
        ]
      },
      {
        text: 'Design',
        items: [
          { text: 'Design index', link: '/design/' },
          { text: 'Product and requirements', link: '/design/product/' },
          { text: 'Architecture', link: '/design/architecture/' },
          { text: 'Decisions', link: '/design/decisions/' },
          { text: 'Documentation skills', link: '/design/documentation-skills' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/PlagueHO/our-iq' }
    ],
    search: {
      provider: 'local'
    },
    editLink: {
      pattern: 'https://github.com/PlagueHO/our-iq/edit/main/docs/:path'
    },
    footer: {
      message: 'Released under the MIT License.'
    }
  }
})
